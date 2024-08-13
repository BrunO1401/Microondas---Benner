using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class cadastroPrograma : Form
    {
        // Define um delegate para o evento de fechamento.
        public delegate void cadastroProgramaEventHandler(object sender, EventArgs e);
        public event cadastroProgramaEventHandler eventoFechar;

        // Define um caractere padrão para comparação.
        private string charPadrao = ".";

        // String de conexão com o banco de dados.
        private string connectionString = "Server=localhost\\SQLEXPRESS;Database=ListaPrograma;Trusted_Connection=True;TrustServerCertificate=True;";

        public cadastroPrograma()
        {
            InitializeComponent();
        }

        // Evento que ocorre quando o formulário é carregado.
        private void cadastroPrograma_Load(object sender, EventArgs e)
        {
            // Verifica a conexão com o banco de dados ao carregar o formulário.
            VerificarConexao();
        }        

        // Verifica a conexão com o banco de dados.
        private void VerificarConexao()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    MessageBox.Show("Conexão com o banco de dados bem-sucedida!");
                }
            }
            catch (Exception ex)
            {
                // Exibe mensagem de erro caso ocorra uma exceção.
                MessageBox.Show("Erro ao conectar ao banco de dados: " + ex.Message);
            }
        }

        // Verifica se o caractere de aquecimento já existe no banco de dados.
        private bool CharAquecimentoExiste(string charAquecimento)
        {
            bool existe = false;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(*) FROM Programas WHERE char_Aquecimento = @char_Aquecimento";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@char_Aquecimento", charAquecimento);
                    try
                    {
                        connection.Open();
                        // Executa a consulta e verifica se o caractere existe.
                        int count = (int)command.ExecuteScalar();
                        existe = count > 0;
                    }
                    catch (Exception ex)
                    {
                        // Exibe mensagem de erro caso ocorra uma exceção.
                        MessageBox.Show("Erro ao verificar caractere de aquecimento: " + ex.Message);
                    }
                }
            }
            return existe;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            // Verifica se o valor da potência é válido.
            if (Convert.ToInt32(txtPotencia.Text) > 10)
            {
                MessageBox.Show("Valor Potência Inválido");
                return;
            }

            // Verifica se todos os campos obrigatórios estão preenchidos.
            if (string.IsNullOrEmpty(txtNomePrograma.Text) ||
                string.IsNullOrEmpty(txtPotencia.Text) ||
                string.IsNullOrEmpty(txtAlimento.Text) ||
                string.IsNullOrEmpty(txtTempo.Text) ||
                string.IsNullOrEmpty(txtCharAquecimento.Text))
            {
                MessageBox.Show("Campos obrigatórios não preenchidos.");// Informa em tela se algum campo está nulo.
                return;
            }

            // Verifica se o char_Aquecimento já existe ou é o caractere padrão.
            if ((CharAquecimentoExiste(txtCharAquecimento.Text)) || txtCharAquecimento.Text == charPadrao)
            {
                MessageBox.Show("O caractere de aquecimento fornecido já existe em outro programa.");
                return; // Interrompe o método e não prossegue com a inserção.
            }

            // Grava o novo programa de aquecimento no banco de dados.
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO Programas (Nome_Programa, Potencia, Alimento, Tempo, char_Aquecimento, Instrucao) VALUES (@Nome_Programa, @Potencia, @Alimento, @Tempo, @char_Aquecimento, @Instrucao)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Adiciona parâmetros para a consulta SQL.
                    command.Parameters.AddWithValue("@Nome_Programa", txtNomePrograma.Text);
                    command.Parameters.AddWithValue("@Potencia", txtPotencia.Text);
                    command.Parameters.AddWithValue("@Alimento", txtAlimento.Text);
                    command.Parameters.AddWithValue("@Tempo", txtTempo.Text);
                    command.Parameters.AddWithValue("@char_Aquecimento", txtCharAquecimento.Text);
                    command.Parameters.AddWithValue("@Instrucao", txtInstrucaoUso.Text);

                    try
                    {
                        // Abre a conexão e executa a consulta.
                        connection.Open();
                        command.ExecuteNonQuery();
                        MessageBox.Show("Configuração adicionada e salva!");

                        // Fecha o formulário e dispara o evento de fechamento, se inscrito.
                        this.Close();
                        eventoFechar?.Invoke(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        // Exibe mensagem de erro caso ocorra uma exceção.
                        MessageBox.Show("Erro ao salvar a configuração: " + ex.Message);
                    }
                }
            }
        }

        private void btnCancelar_Click_1(object sender, EventArgs e)
        {
            // Verifica se algum campo está preenchido e limpa os campos se necessário.
            if (string.IsNullOrEmpty(txtNomePrograma.Text) ||
                string.IsNullOrEmpty(txtPotencia.Text) ||
                string.IsNullOrEmpty(txtAlimento.Text) ||
                string.IsNullOrEmpty(txtTempo.Text) ||
                string.IsNullOrEmpty(txtCharAquecimento.Text) ||
                string.IsNullOrEmpty(txtInstrucaoUso.Text))
            {
                // Fecha o formulário se todos os campos estiverem vazios.
                this.Close();
            }
            else
            {
                // Limpa todos os campos se não estiverem todos vazios.
                txtNomePrograma.Text = string.Empty;
                txtPotencia.Text = string.Empty;
                txtAlimento.Text = string.Empty;
                txtTempo.Text = string.Empty;
                txtCharAquecimento.Text = string.Empty;
                txtInstrucaoUso.Text = string.Empty;
            }
        }
    }
}
